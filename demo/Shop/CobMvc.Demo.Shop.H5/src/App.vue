<template>
  <div class="m-3" id="app">
    <div>
      <div class="row">
        <div class="col-sm-8">
          <h3 class="text-left">商品列表</h3>
          <hr/>
          <ProductList v-if="model&&model.product&&model.product.data" :items="model.product.data" :showCheckbox="true" v-on:product:item:selected="productItemSelected"></ProductList>
        </div>
        <div class="col-sm-4">
          <transition name="cart">
            <div class="content position-relative" v-if="cart.length">
              <h3 class="text-left">购物车</h3>
              <hr/>
              <ProductList :items="cart"></ProductList>
              <button type="button" class="btn btn-primary pull-right mx-2" :disabled="!cart.length" data-toggle="modal" data-target="#dlgAddr">确定</button>
              <!-- <button type="button" class="btn btn-default pull-right mx-2" :disabled="!cart.length" @click="resetCart()">清空</button> -->
            </div>
          </transition>
        </div>
      </div>
    </div>
    <Dialog id="dlgAddr" title="选择地址" :showFooter="true" :onConfirm="confirmOrder">
      <AddressList :items="model.user.data.address" ref="addr"></AddressList>
    </Dialog>
  </div>
  
</template>

<script>
import Vue from 'vue'
import ProductList from './components/product/List.vue'
import AddressList from './components/user/Address.vue'
import Dialog from './components/common/dialog.vue'

export default {
  name: 'app',
  components: {
    ProductList,
    AddressList,
    Dialog
  },
  data(){
    return {
      model:null,
      cart:[]
    }
  },
  mounted(){
    Vue.axios.get('/gw/vapi/index', {
      params:{
        userID:0
        }
      }).then(r=>{
        this.model = r.data;
    });
  },
  methods:{
    productItemSelected(isSelected, item){
      if(isSelected)
        this.cart.push(item);
      else{
        this.cart.splice(this.cart.findIndex(v=>v.id == item.id), 1);
      }
    },
    resetCart(){
      this.cart.splice(0, 0);
    },
    confirmOrder(){
      let addr = this.$refs['addr'].getSelectedAddress();
      if(!addr){
        return alert('请选择地址');
      }
      
      if(!this.cart.length){
        return alert('请选择商品');
      }

      Vue.axios.post('/gw/api/order/CreateOrder', {
        UserID:this.model.user.data.id,
        Address:addr.id,
        Details:this.cart.map(c=>{
          return {ProductID:c.id, ProductName:c.name, Quality:1};
        })
      }).then(r=>{
        if(r.data.isSuccess){
          alert('下单成功:'+r.data.data.id)
        }
        else{
          alert(r.data.message);
        }
      })
    }
  }
}
</script>

<style>
#app {
  font-family: 'Avenir', Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
  margin-top: 60px;
}

.product-item label{
  cursor: pointer;
}

.cart-enter-active , .cart-leave-active{
  opacity: 1;
  margin-left: 0;
  transition: all .5s;
  transition-property: opacity, margin-left;
}

.cart-enter, .cart-leave-to{
  opacity: 0;
  margin-left: 30%
}

.product-item-enter, .product-item-leave-to{
  opacity: 0;
}

.product-item-enter-active, .product-item-leave-active{
  transition: all .5s;
}
</style>
